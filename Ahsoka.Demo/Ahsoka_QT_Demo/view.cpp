#include "view.h"
#include "ui_view.h"
#include "Gauge.h"
#include "minigauge.h"

#include <QGraphicsDropShadowEffect>

View::View(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::View)
{
    ui->setupUi(this);

    //Set Background
    QPixmap bkgnd(":/Resources/bg_scaled.png");
    bkgnd = bkgnd.scaled(this->size(), Qt::IgnoreAspectRatio);
    QPalette palette;
    palette.setBrush(QPalette::Window, bkgnd);
    this->setPalette(palette);

    //Connect Buttons to their functions
    connect(ui->Demo_Btn, &QPushButton::clicked, this, &View::Demo_Btn_Pressed);
    connect(ui->Display_Btn, &QPushButton::clicked, this, &View::Display_Btn_Pressed);
    connect(ui->Electrical_Btn, &QPushButton::clicked, this, &View::Electrical_Btn_Pressed);
    connect(ui->Hardware_Btn, &QPushButton::clicked, this, &View::Hardware_Btn_Pressed);
    connect(ui->Interfaces_Btn, &QPushButton::clicked, this, &View::Interfaces_Btn_Pressed);
    connect(ui->MoreInfo_Btn, &QPushButton::clicked, this, &View::MoreInfo_Btn_Pressed);
    connect(ui->Software_Btn, &QPushButton::clicked, this, &View::Software_Btn_Pressed);

    connect(ui->Demo_Home_Btn, &QPushButton::clicked, this, &View::Home_Btn_Pressed);
    connect(ui->Display_Home_Btn, &QPushButton::clicked, this, &View::Home_Btn_Pressed);
    connect(ui->Electrical_Home_Btn, &QPushButton::clicked, this, &View::Home_Btn_Pressed);
    connect(ui->Hardware_Home_Btn, &QPushButton::clicked, this, &View::Home_Btn_Pressed);
    connect(ui->Interfaces_Home_Btn, &QPushButton::clicked, this, &View::Home_Btn_Pressed);
    connect(ui->MoreInfo_Home_Btn, &QPushButton::clicked, this, &View::Home_Btn_Pressed);
    connect(ui->Software_Home_Btn, &QPushButton::clicked, this, &View::Home_Btn_Pressed);

    //Set button images
    ui->Interfaces_Btn->setStyleSheet("border-image:url(:/Resources/Asset 4.png);");
    ui->Interfaces_Btn->setGeometry(
                ui->Interfaces_Btn->geometry().x(),
                ui->Interfaces_Btn->geometry().y(),
                121,
                170
    );
    ui->Display_Btn->setStyleSheet("border-image:url(:/Resources/Asset 3.png);");
    ui->Display_Btn->setGeometry(
                ui->Display_Btn->geometry().x(),
                ui->Display_Btn->geometry().y(),
                137,
                169
    );
    ui->Electrical_Btn->setStyleSheet("border-image:url(:/Resources/Asset 2.png);");
    ui->Electrical_Btn->setGeometry(
                ui->Electrical_Btn->geometry().x(),
                ui->Electrical_Btn->geometry().y(),
                123,
                173
    );
    ui->Demo_Btn->setStyleSheet("border-image:url(:/Resources/Asset 1.png);");
    ui->Demo_Btn->setGeometry(
                ui->Demo_Btn->geometry().x(),
                ui->Demo_Btn->geometry().y(),
                152,
                175
    );
    ui->Hardware_Btn->setStyleSheet("border-image:url(:/Resources/Asset 5.png);");
    ui->Hardware_Btn->setGeometry(
                ui->Hardware_Btn->geometry().x(),
                ui->Hardware_Btn->geometry().y(),
                166,
                173
    );
    ui->Software_Btn->setStyleSheet("border-image:url(:/Resources/Asset 6.png);");
    ui->Software_Btn->setGeometry(
                ui->Software_Btn->geometry().x(),
                ui->Software_Btn->geometry().y(),
                167,
                188
    );
    ui->MoreInfo_Btn->setStyleSheet("border-image:url(:/Resources/Asset 7.png);");
    ui->MoreInfo_Btn->setGeometry(
                ui->MoreInfo_Btn->geometry().x(),
                ui->MoreInfo_Btn->geometry().y(),
                160,
                187
    );
    ui->Interfaces_Home_Btn->setStyleSheet("border-image:url(:/Resources/Asset 9.png);");
    ui->Interfaces_Home_Btn->setGeometry(
                ui->Interfaces_Home_Btn->geometry().x(),
                ui->Interfaces_Home_Btn->geometry().y(),
                68,
                91
    );
    ui->Display_Home_Btn->setStyleSheet("border-image:url(:/Resources/Asset 9.png);");
    ui->Display_Home_Btn->setGeometry(
                ui->Display_Home_Btn->geometry().x(),
                ui->Display_Home_Btn->geometry().y(),
                68,
                91
    );
    ui->Electrical_Home_Btn->setStyleSheet("border-image:url(:/Resources/Asset 9.png);");
    ui->Electrical_Home_Btn->setGeometry(
                ui->Electrical_Home_Btn->geometry().x(),
                ui->Electrical_Home_Btn->geometry().y(),
                68,
                91
    );
    ui->Demo_Home_Btn->setStyleSheet("border-image:url(:/Resources/Asset 9.png);");
    ui->Demo_Home_Btn->setGeometry(
                ui->Demo_Home_Btn->geometry().x(),
                ui->Demo_Home_Btn->geometry().y(),
                68,
                91
    );
    ui->Hardware_Home_Btn->setStyleSheet("border-image:url(:/Resources/Asset 9.png);");
    ui->Hardware_Home_Btn->setGeometry(
                ui->Hardware_Home_Btn->geometry().x(),
                ui->Hardware_Home_Btn->geometry().y(),
                68,
                91
    );
    ui->Software_Home_Btn->setStyleSheet("border-image:url(:/Resources/Asset 9.png);");
    ui->Software_Home_Btn->setGeometry(
                ui->Software_Home_Btn->geometry().x(),
                ui->Software_Home_Btn->geometry().y(),
                68,
                91
    );
    ui->MoreInfo_Home_Btn->setStyleSheet("border-image:url(:/Resources/Asset 9.png);");
    ui->MoreInfo_Home_Btn->setGeometry(
                ui->MoreInfo_Home_Btn->geometry().x(),
                ui->MoreInfo_Home_Btn->geometry().y(),
                68,
                91
    );

    //Add Gauges

    Gauge* gauge = new Gauge(this->ui->Demo);
    gauge->setGeometry(
                300,
                60,
                this->geometry().width(),
                this->geometry().height()
                );
    gauge->show();

    MiniGauge* minigauge_1 = new MiniGauge(this->ui->Demo);
    minigauge_1->setGeometry(
                100,
                150,
                this->geometry().width(),
                this->geometry().height()
                );
    minigauge_1->show();

    MiniGauge* minigauge_2 = new MiniGauge(this->ui->Demo);
    minigauge_2->setGeometry(
                100,
                270,
                this->geometry().width(),
                this->geometry().height()
                );
    minigauge_2->show();

    this->ui->stackedWidget->setCurrentIndex(0); //Index 0 -> Home
}

View::~View()
{
    delete ui;
}

void View::Demo_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(4); //Index 4 -> Demo
}

void View::Display_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(1); //Index 1 -> Display
}

void View::Electrical_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(3); //Index 3 -> Electrical
}

void View::Hardware_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(5); //Index 5 -> Hardware
}

void View::Interfaces_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(2); //Index 2 -> Interfaces
}

void View::MoreInfo_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(7); //Index 7 -> More Info
}

void View::Software_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(6); //Index 6 -> Software
}

void View::Home_Btn_Pressed()
{
    this->ui->stackedWidget->setCurrentIndex(0); //Index 0 -> Home
}
